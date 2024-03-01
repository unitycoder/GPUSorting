/******************************************************************************
 * GPUSorting
 *
 * SPDX-License-Identifier: MIT
 * Author:  Thomas Smith 2/28/2024
 * https://github.com/b0nes164/GPUSorting
 * 
 ******************************************************************************/
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Assertions;

public class GPUSortBase
{
    protected const int k_radix = 256;
    protected const int k_radixPasses = 4;
    protected const int k_keysPartitionSize = 3840;
    protected const int k_pairsPartitionSize = 7680;

    protected const int k_minSize = 1;
    protected const int k_maxSize = int.MaxValue;

    protected ComputeShader m_cs;

    protected LocalKeyword m_keyIntKeyword;
    protected LocalKeyword m_keyUintKeyword;
    protected LocalKeyword m_keyFloatKeyword;
    protected LocalKeyword m_payloadIntKeyword;
    protected LocalKeyword m_payloadUintKeyword;
    protected LocalKeyword m_payloadFloatKeyword;
    protected LocalKeyword m_ascendKeyword;
    protected LocalKeyword m_sortPairKeyword;

    protected int m_numKeys;
    protected int m_threadBlocks;

    protected bool m_staticMemory = false;
    
    protected static int DivRoundUp(int x, int y)
    {
        return (x + y - 1) / y;
    }

    protected void InitializeKeywords()
    {
        m_ascendKeyword = new LocalKeyword(m_cs, "SHOULD_ASCEND");
        m_sortPairKeyword = new LocalKeyword(m_cs, "SORT_PAIRS");
        m_keyUintKeyword = new LocalKeyword(m_cs, "KEY_UINT");
        m_keyIntKeyword = new LocalKeyword(m_cs, "KEY_INT");
        m_keyFloatKeyword = new LocalKeyword(m_cs, "KEY_FLOAT");
        m_payloadUintKeyword = new LocalKeyword(m_cs, "PAYLOAD_UINT");
        m_payloadIntKeyword = new LocalKeyword(m_cs, "PAYLOAD_INT");
        m_payloadFloatKeyword = new LocalKeyword(m_cs, "PAYLOAD_FLOAT");
    }

    protected void SetKeyTypeKeywords(System.Type _type)
    {
        if (_type == typeof(int))
        {
            m_cs.EnableKeyword(m_keyIntKeyword);
            m_cs.DisableKeyword(m_keyUintKeyword);
            m_cs.DisableKeyword(m_keyFloatKeyword);
        }

        if (_type == typeof(uint))
        {
            m_cs.DisableKeyword(m_keyIntKeyword);
            m_cs.EnableKeyword(m_keyUintKeyword);
            m_cs.DisableKeyword(m_keyFloatKeyword);
        }

        if (_type == typeof(float))
        {
            m_cs.DisableKeyword(m_keyIntKeyword);
            m_cs.DisableKeyword(m_keyUintKeyword);
            m_cs.EnableKeyword(m_keyFloatKeyword);
        }
    }

    protected void SetKeyTypeKeywords(CommandBuffer _cmd, System.Type _type)
    {
        if (_type == typeof(int))
        {
            _cmd.EnableKeyword(m_cs, m_keyIntKeyword);
            _cmd.DisableKeyword(m_cs, m_keyUintKeyword);
            _cmd.DisableKeyword(m_cs, m_keyFloatKeyword);
        }

        if (_type == typeof(uint))
        {
            _cmd.DisableKeyword(m_cs, m_keyIntKeyword);
            _cmd.EnableKeyword(m_cs, m_keyUintKeyword);
            _cmd.DisableKeyword(m_cs, m_keyFloatKeyword);
        }

        if (_type == typeof(float))
        {
            _cmd.DisableKeyword(m_cs, m_keyIntKeyword);
            _cmd.DisableKeyword(m_cs, m_keyUintKeyword);
            _cmd.EnableKeyword(m_cs, m_keyFloatKeyword);
        }
    }

    protected void SetPayloadTypeKeywords(System.Type _type)
    {
        if (_type == typeof(int))
        {
            m_cs.EnableKeyword(m_payloadIntKeyword);
            m_cs.DisableKeyword(m_payloadUintKeyword);
            m_cs.DisableKeyword(m_payloadFloatKeyword);
        }

        if (_type == typeof(uint))
        {
            m_cs.DisableKeyword(m_payloadIntKeyword);
            m_cs.EnableKeyword(m_payloadUintKeyword);
            m_cs.DisableKeyword(m_payloadFloatKeyword);
        }

        if (_type == typeof(float))
        {
            m_cs.DisableKeyword(m_payloadIntKeyword);
            m_cs.DisableKeyword(m_payloadUintKeyword);
            m_cs.EnableKeyword(m_payloadFloatKeyword);
        }
    }

    protected void SetPayloadTypeKeywords(CommandBuffer _cmd, System.Type _type)
    {
        if (_type == typeof(int))
        {
            _cmd.EnableKeyword(m_cs, m_payloadIntKeyword);
            _cmd.DisableKeyword(m_cs, m_payloadUintKeyword);
            _cmd.DisableKeyword(m_cs, m_payloadFloatKeyword);
        }

        if (_type == typeof(uint))
        {
            _cmd.DisableKeyword(m_cs, m_payloadIntKeyword);
            _cmd.EnableKeyword(m_cs, m_payloadUintKeyword);
            _cmd.DisableKeyword(m_cs, m_payloadFloatKeyword);
        }

        if (_type == typeof(float))
        {
            _cmd.DisableKeyword(m_cs, m_payloadIntKeyword);
            _cmd.DisableKeyword(m_cs, m_payloadUintKeyword);
            _cmd.EnableKeyword(m_cs, m_payloadFloatKeyword);
        }
    }

    protected void SetAscendingKeyWords(bool _shouldAscend)
    {
        if (_shouldAscend)
            m_cs.EnableKeyword(m_ascendKeyword);
        else
            m_cs.DisableKeyword(m_ascendKeyword);
    }

    protected void SetAscendingKeyWords(CommandBuffer _cmd, bool _shouldAscend)
    {
        if (_shouldAscend)
            _cmd.EnableKeyword(m_cs, m_ascendKeyword);
        else
            _cmd.DisableKeyword(m_cs, m_ascendKeyword);
    }

    protected bool UpdateSizeKeysOnly(int inputNumKeys)
    {
        if (inputNumKeys != m_numKeys)
        {
            m_numKeys = inputNumKeys;
            m_threadBlocks = DivRoundUp(inputNumKeys, k_keysPartitionSize);
            return true;
        }
        else
        {
            if (!m_staticMemory)
            {
                int t = DivRoundUp(inputNumKeys, k_keysPartitionSize);
                if (t != m_threadBlocks)
                {
                    m_numKeys = inputNumKeys;
                    m_threadBlocks = t;
                    return true;
                }
            }
        }
        return false;
    }

    protected bool UpdateSizePairs(int inputNumKeys)
    {
        if (inputNumKeys != m_numKeys)
        {
            m_numKeys = inputNumKeys;
            m_threadBlocks = DivRoundUp(inputNumKeys, k_pairsPartitionSize);
            return true;
        }
        else
        {
            if (!m_staticMemory)
            {
                int t = DivRoundUp(inputNumKeys, k_pairsPartitionSize);
                if (t != m_threadBlocks)
                {
                    m_numKeys = inputNumKeys;
                    m_threadBlocks = t;
                    return true;
                }
            }
        }
        return false;
    }
}